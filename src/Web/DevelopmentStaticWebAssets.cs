namespace Web;

public static class DevelopmentStaticWebAssets
{
	public static void EnableForDevelopment(WebApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		if (builder.Environment.IsDevelopment())
		{
			builder.WebHost.UseStaticWebAssets();
		}
	}
}
