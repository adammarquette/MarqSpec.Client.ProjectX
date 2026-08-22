using MarqSpec.Client.ProjectX.FakeGateway;

// Container entry point. The wiring lives in FakeGatewayHost so the integration tests can host the exact same
// application in-process, rather than testing against a second, subtly different assembly.
await FakeGatewayHost.Build(args).RunAsync();
