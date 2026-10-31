export async function handler() {
  const CLIENT_ID = process.env.DISCORD_CLIENT_ID;
  const REDIRECT_URI = process.env.DISCORD_REDIRECT_URI;

  const url = new URL('https://discord.com/oauth2/authorize');
  url.searchParams.set('client_id', CLIENT_ID);
  url.searchParams.set('redirect_uri', REDIRECT_URI);
  url.searchParams.set('response_type', 'code');
  url.searchParams.set('scope', 'identify email');

  return {
    statusCode: 302,
    headers: { Location: url.toString() },
    body: ''
  };
}