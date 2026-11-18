interface Env {
  TOMATIME_API_ORIGIN: string;
}

interface PagesContext {
  request: Request;
  env: Env;
}

export const onRequest = async ({ request, env }: PagesContext): Promise<Response> => {
  let apiOrigin: URL;
  try {
    apiOrigin = new URL(env.TOMATIME_API_ORIGIN);
  } catch {
    return new Response('API proxy is not configured.', { status: 500 });
  }

  if (apiOrigin.protocol !== 'https:' || apiOrigin.username || apiOrigin.password) {
    return new Response('API proxy configuration is invalid.', { status: 500 });
  }

  const target = new URL(request.url);
  target.protocol = apiOrigin.protocol;
  target.host = apiOrigin.host;

  const upstreamRequest = new Request(target, request);
  const headers = new Headers(upstreamRequest.headers);
  headers.delete('Origin');
  headers.delete('Host');

  return fetch(new Request(upstreamRequest, { headers }));
};
