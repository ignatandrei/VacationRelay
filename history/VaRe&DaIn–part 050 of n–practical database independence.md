When I think about database independence, I need to consider two perspectives: the programmer and the application administrator.

As a programmer, I want debugging to be straightforward, regardless of which database I am using. I also want to understand how to develop and test against any supported database without having to redesign the application for each provider.

As an administrator, I want to be able to add database providers when necessary, as long as those providers are available to download and install.

For the programmer, the simplest approach is to work with the database providers that are already installed locally.

For the administrator, I want to be able to choose a provider from the local installation or download one from the internet when it is not available locally.

To support both perspectives, I need the following components:

10. `VersionClass` first determines the required version, such as EF Core 10 or EF Core 11.

20. `LocalLoaderClass` loads the locally installed database providers that are compatible with the version selected by `VersionClass`.

30. `InternetLoaderClass` discovers the remaining providers available online, compares them with the providers found by `LocalLoaderClass`, and downloads a provider on request when it is needed.

40. `ConfigureStart` configures the database through the selected provider, including the database schema, tables, and any other required resources. 

See the picture

<p>
<img src="https://ignatandrei.github.io/VacationRelay/DaIn/VaRe&DaIn050.svg" alt="Loader">
</p>