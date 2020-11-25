# NPitaya
This folder contains the C# implementation of Pitaya. It uses the Rust library under the hood.

# Structure
The main implementation of the code is located under the `NPitaya` folder. The other folders are used for testing.

# Generating a Nupkg
This library is shared using Nupkg. You can create one with the following command:
```
make pack
```

# Running the Example Project
You can run the example project with the following commands:
```
(cd .. && docker-compose up -d) # startup dependencies if you didn't do that yet.
dotnet run -p exampleapp
```