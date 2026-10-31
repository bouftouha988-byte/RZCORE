const express = require('express');
const session = require('express-session');
const passport = require('passport');
const DiscordStrategy = require('passport-discord').Strategy;

const app = express();

const CLIENT_ID     = '1486434070723301396';
const CLIENT_SECRET = process.env.DISCORD_SECRET;
const CALLBACK_URL  = 'http://localhost:3000/auth/discord/callback';
const PORT          = 3000;

if (!CLIENT_SECRET) {
  console.error('\n❌ DISCORD_SECRET manquant.');
  console.error('Windows PowerShell : $env:DISCORD_SECRET="ton_secret"');
  console.error('Puis relance : node server.js\n');
  process.exit(1);
}

app.use(session({
  secret: 'rzcore-session-secret-change-moi',
  resave: false,
  saveUninitialized: false,
  cookie: { secure: false }
}));

app.use(passport.initialize());
app.use(passport.session());

passport.use(new DiscordStrategy({
    clientID: CLIENT_ID,
    clientSecret: CLIENT_SECRET,
    callbackURL: CALLBACK_URL,
    scope: ['identify', 'email']
  },
  (accessToken, refreshToken, profile, done) => done(null, profile)
));

passport.serializeUser((user, done) => done(null, user));
passport.deserializeUser((obj, done) => done(null, obj));

app.get('/auth/discord', passport.authenticate('discord'));

app.get('/auth/discord/callback',
  passport.authenticate('discord', { failureRedirect: '/#login' }),
  (req, res) => res.redirect('/#dashboard')
);

app.get('/api/me', (req, res) => {
  if (req.isAuthenticated()) {
    res.json({
      user: {
        id: req.user.id,
        name: req.user.username,
        avatar: req.user.avatar
          ? `https://cdn.discordapp.com/avatars/${req.user.id}/${req.user.avatar}.png?size=128`
          : '',
        role: 'user'
      }
    });
  } else {
    res.json({ user: null });
  }
});

app.get('/logout', (req, res) => {
  req.logout(() => res.redirect('/#home'));
});

app.use(express.static(__dirname));

app.listen(PORT, () => {
  console.log(`\n✅ RzCore démarré → http://localhost:${PORT}\n`);
});