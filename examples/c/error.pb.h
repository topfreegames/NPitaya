/* Automatically generated nanopb header */
/* Generated by nanopb-0.4.2-dev */

#ifndef PB_PROTOS_ERROR_PB_H_INCLUDED
#define PB_PROTOS_ERROR_PB_H_INCLUDED
#include <pb.h>

#if PB_PROTO_HEADER_VERSION != 40
#error Regenerate this file with the current version of nanopb generator.
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Struct definitions */
typedef struct _protos_Error {
    pb_callback_t code;
    pb_callback_t msg;
    pb_callback_t metadata;
} protos_Error;

typedef struct _protos_Error_MetadataEntry {
    pb_callback_t key;
    pb_callback_t value;
} protos_Error_MetadataEntry;


/* Initializer values for message structs */
#define protos_Error_init_default                {{{NULL}, NULL}, {{NULL}, NULL}, {{NULL}, NULL}}
#define protos_Error_MetadataEntry_init_default  {{{NULL}, NULL}, {{NULL}, NULL}}
#define protos_Error_init_zero                   {{{NULL}, NULL}, {{NULL}, NULL}, {{NULL}, NULL}}
#define protos_Error_MetadataEntry_init_zero     {{{NULL}, NULL}, {{NULL}, NULL}}

/* Field tags (for use in manual encoding/decoding) */
#define protos_Error_code_tag                    1
#define protos_Error_msg_tag                     2
#define protos_Error_metadata_tag                3
#define protos_Error_MetadataEntry_key_tag       1
#define protos_Error_MetadataEntry_value_tag     2

/* Struct field encoding specification for nanopb */
#define protos_Error_FIELDLIST(X, a) \
X(a, CALLBACK, SINGULAR, STRING,   code,              1) \
X(a, CALLBACK, SINGULAR, STRING,   msg,               2) \
X(a, CALLBACK, REPEATED, MESSAGE,  metadata,          3)
#define protos_Error_CALLBACK pb_default_field_callback
#define protos_Error_DEFAULT NULL
#define protos_Error_metadata_MSGTYPE protos_Error_MetadataEntry

#define protos_Error_MetadataEntry_FIELDLIST(X, a) \
X(a, CALLBACK, SINGULAR, STRING,   key,               1) \
X(a, CALLBACK, SINGULAR, STRING,   value,             2)
#define protos_Error_MetadataEntry_CALLBACK pb_default_field_callback
#define protos_Error_MetadataEntry_DEFAULT NULL

extern const pb_msgdesc_t protos_Error_msg;
extern const pb_msgdesc_t protos_Error_MetadataEntry_msg;

/* Defines for backwards compatibility with code written before nanopb-0.4.0 */
#define protos_Error_fields &protos_Error_msg
#define protos_Error_MetadataEntry_fields &protos_Error_MetadataEntry_msg

/* Maximum encoded size of messages (where known) */
/* protos_Error_size depends on runtime parameters */
/* protos_Error_MetadataEntry_size depends on runtime parameters */

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif
