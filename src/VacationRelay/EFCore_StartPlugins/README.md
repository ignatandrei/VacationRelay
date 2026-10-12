# Database plugin selector

The home page lists providers in two rows, using the plugin's `NeedServer` flag.
Select a provider and enter a connection string and database name. **Test connection**
calls `POST /api/dain/plugins/testConnection`, which invokes
`IDatabasePlugin.TestConnectionAsync` on the API host. **Save chosen plugin** calls
`POST /api/dain/plugins/defaultPlugin` to persist the selection with the plugin loader.
Saving is enabled only after a successful connection test. Changing the provider,
connection string, or database name disables saving until the connection is tested
successfully again. A failed test also disables saving.
The connection string and database name may be empty, depending on the provider.
The selected plugin determines whether the supplied connection string is valid
when testing; the UI and API do not require a non-empty value.

Run through the VacationRelay AppHost to start the separate `plugin-ui` host and
`VR-WebAPI`. Aspire supplies the API endpoint and allowed browser origins.

For standalone development, run `VR.WebAPI` with its `http` launch profile and
`EFCore_StartPlugins.Host` with its `http` launch profile. Open
`http://localhost:5242`. Run the host project rather than the client's WASM dev server:
the host serves the Blazor assets and `/client-configuration`.

Outside Aspire, configure `ApiBaseUrl` on the UI host and `PluginUiOrigins` on the
API. Use HTTPS for both applications in production. The connection string is sent
to the API for testing/saving, but is not returned by the saved-provider endpoint.
This configuration UI and API are intended for trusted local use; restrict access
before exposing them to other users.
