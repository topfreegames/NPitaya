use crate::{
    cluster::{self},
    constants,
    context::Context,
    handler::Handlers,
    protos,
    session::{self, Session},
    utils, Route,
};
use slog::{debug, error, o, warn};
use std::sync::Arc;
use tokio::sync::Mutex;

pub type RpcHandler = Box<dyn Fn(Context, Option<Session>, cluster::Rpc) + Send + Sync + 'static>;

pub enum RpcDispatch {
    Handlers {
        client: Arc<Handlers>,
        server: Arc<Handlers>,
    },
    Raw(RpcHandler),
}

// Remote is a service that is capable of handling RPC messages from other pitaya servers
// and also sending RPCs.
pub struct Remote {
    logger: slog::Logger,
    discovery: Arc<Mutex<Box<dyn cluster::Discovery>>>,
    rpc_client: Arc<Box<dyn cluster::RpcClient>>,
    rpc_dispatch: RpcDispatch,
}

impl Remote {
    pub fn new(
        logger: slog::Logger,
        discovery: Arc<Mutex<Box<dyn cluster::Discovery>>>,
        rpc_client: Arc<Box<dyn cluster::RpcClient>>,
        rpc_dispatch: RpcDispatch,
    ) -> Self {
        Self {
            logger,
            discovery,
            rpc_client,
            rpc_dispatch,
        }
    }

    pub async fn process_rpc(&self, ctx: Context, rpc: cluster::Rpc) {
        let route = {
            if rpc.request().msg.is_none() {
                warn!(self.logger, "received rpc without message");
                if !rpc.respond(utils::build_error_response(
                    constants::CODE_BAD_FORMAT,
                    "received RPC without message",
                )) {
                    error!(self.logger, "failed to respond to rpc");
                }
                return;
            }

            let route_str = rpc.request().msg.as_ref().unwrap().route.to_string();
            let maybe_route = Route::try_from_str(route_str.clone());

            if maybe_route.is_none() {
                warn!(self.logger, "received rpc with invalid route"; "route" => %route_str);
                if !rpc.respond(utils::build_error_response(
                    constants::CODE_BAD_FORMAT,
                    format!("invalid route: {}", route_str),
                )) {
                    error!(self.logger, "failed to respond to rpc");
                }
                return;
            }

            maybe_route.unwrap()
        };

        match protos::RpcType::from_i32(rpc.request().r#type) {
            Some(protos::RpcType::User) => self.process_rpc_user(ctx, rpc, route).await,
            Some(protos::RpcType::Sys) => self.process_rpc_sys(ctx, rpc, route).await,
            None => {
                error!(self.logger, "received unknown rpc type");
                if !rpc.respond(utils::build_error_response(
                    constants::CODE_BAD_FORMAT,
                    format!("invalid route: {}", route.as_str()),
                )) {
                    error!(self.logger, "failed to respond to rpc");
                }
            }
        }
    }

    async fn process_rpc_user(&self, ctx: Context, rpc: cluster::Rpc, route: Route) {
        debug!(self.logger, "processing user rpc");
        // Having the route, we need to find the correct handler and method for it.
        match &self.rpc_dispatch {
            RpcDispatch::Handlers { server, .. } => {
                Self::call_method_and_respond(
                    self.logger.clone(),
                    server.clone(),
                    ctx,
                    None,
                    rpc,
                    route,
                )
                .await;
            }
            RpcDispatch::Raw(rpc_handler) => {
                rpc_handler(ctx, None, rpc);
            }
        }
    }

    async fn process_rpc_sys(&self, ctx: Context, rpc: cluster::Rpc, route: Route) {
        debug!(self.logger, "processing sys rpc");

        let req = rpc.request();

        // Get session object
        let session = match Session::new(
            self.logger.new(o!()),
            req,
            self.rpc_client.clone(),
            self.discovery.clone(),
        ) {
            Ok(s) => s,
            Err(session::Error::SessionNotFound) => {
                warn!(self.logger, "received rpc sys without session");
                if !rpc.respond(utils::build_error_response(
                    constants::CODE_BAD_FORMAT,
                    "was expecting session object",
                )) {
                    error!(self.logger, "failed to respond to rpc");
                }
                return;
            }
            Err(session::Error::CorruptedSessionData(s)) => {
                warn!(self.logger, "received rpc with corrupt data"; "data" => %s);
                if !rpc.respond(utils::build_error_response(
                    constants::CODE_BAD_FORMAT,
                    format!("corrupt session data: {}", s),
                )) {
                    error!(self.logger, "failed to respond to rpc");
                }
                return;
            }
            _ => unreachable!(),
        };

        debug!(self.logger, "got RPC with session"; "session" => %session);

        match &self.rpc_dispatch {
            RpcDispatch::Handlers { client, .. } => {
                Self::call_method_and_respond(
                    self.logger.clone(),
                    client.clone(),
                    ctx,
                    Some(session),
                    rpc,
                    route,
                )
                .await;
            }
            RpcDispatch::Raw(rpc_handler) => {
                rpc_handler(ctx, Some(session), rpc);
            }
        }
    }

    async fn call_method_and_respond(
        logger: slog::Logger,
        handlers: Arc<Handlers>,
        ctx: Context,
        session: Option<Session>,
        rpc: cluster::Rpc,
        route: Route,
    ) {
        let maybe_method = handlers.get(&route);

        let response = if maybe_method.is_none() {
            warn!(logger, "route was not found"; "route" => %route.as_str());
            utils::build_error_response(
                constants::CODE_NOT_FOUND,
                format!("route not found: {}", route.as_str()),
            )
        } else {
            let method = maybe_method.unwrap();
            method(ctx, session, rpc.request()).await
        };

        if response.error.is_some() {
            warn!(logger, "handler returning error for RPC");
        }

        if !rpc.respond(response) {
            error!(logger, "failed to respond to rpc");
        }
    }
}
