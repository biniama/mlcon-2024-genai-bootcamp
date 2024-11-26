namespace TalkToYourApp.Client.Features.AI_Integration;

public class ProxyClientHandler : HttpClientHandler
{
	private Uri _baseUri;

	public ProxyClientHandler(Uri baseUri)
	{
		_baseUri = baseUri;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri is not null)
		{
			var path = request.RequestUri.PathAndQuery;
			if (path.StartsWith("/")) path = path[1..];

			request.RequestUri = new Uri(_baseUri, path);
		}

		return base.SendAsync(request, cancellationToken);
	}
}
