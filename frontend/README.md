# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.5.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

The `npm start` script configures a development proxy for `/api` to `http://localhost:5197`; run the backend with its `http` launch profile when using the frontend auth forms.

## Source structure

Application code is organized under `src/app`, with components, models, and services in separate folders:

```text
src/app/
  app.ts                 # Application shell
  app.routes.ts          # Lazy-loaded routes
  i18n/                  # Shared language selection
  components/
    landing-page-component/
    login-component/
    register-component/
    reset-password-component/
  models/                # Auth and user API models
  services/              # Auth API, session storage, HTTP interceptor
```

Keep frontend components, models, and services in their respective folders under `src/app`. The .NET backend has its own independent model and service layers.

Password reset links open `/reset-password?token=...`. The form sends the token and new password to `/api/auth/reset-password`. Configure Gmail as described below to send these links by email.

After login or registration, `/home` shows the authenticated dashboard. Its data comes from `/api/dashboard`, which derives the user from the validated JWT. Focus time sums the focus durations (in minutes) of completed sessions. The rewards screen shows the balance and allows users to buy and activate themes or purchase extra breaks.

The legacy `/api/users/profile/{userId}`, `/api/users/{userId}/rewards`, `/api/users/notifications/{userId}` and `/api/sessions` endpoints require authentication and only allow access to the signed-in user's data. The profile response does not include a password hash. The unused user-by-email and user deletion endpoints, and theme creation, modification and deletion endpoints, are no longer exposed; the application does not implement administrator or account-deletion flows.

Extra breaks bought in the shop are applied at the first break of a later eligible session. A session without breaks, or one cancelled before its first break, leaves the purchase pending. Extra breaks can also be bought during a break and are applied immediately.

The new-session screen starts a spontaneous session with 25–300 minutes of focus in 25-minute blocks. The dashboard can also start the earliest previously planned session, including one whose planned time has passed. The timer includes a five-minute break between focus blocks. Completing the entire session awards two pomos per focus block, once; leaving or reloading cancels it without a reward. The timer requires an active connection: if activity is not received for two minutes, the server cancels the session.

The planning screen saves a future session without starting it. Dates and times are entered in the device's local time zone and sent to the API in UTC.

## Password reset email

The API sends password reset messages using Gmail SMTP with STARTTLS. The link expires after 30 minutes. Sending happens in a bounded in-memory queue so that SMTP response time does not reveal whether an account exists. The API returns the same generic response whether the account exists or delivery fails; delivery failures and a full queue are logged on the API without the address, password or reset link. Messages waiting in memory are lost if the API stops, and there is no automatic retry: after correcting a delivery problem or restarting the API, request a new link.

To configure local development:

1. Enable 2-Step Verification on the Google account that will send the emails. Create an [app password](https://support.google.com/accounts/answer/185833) for Tomatime. App passwords may be unavailable for some accounts (for example, certain work or school accounts).
2. From the repository root, store the sending Gmail address in .NET user secrets, replacing the example with the actual address:

   ```bash
   dotnet user-secrets set "Gmail:Address" "your-address@gmail.com" --project backend/src/Tomatime.Api
   ```

3. Store the app password without putting it into shell history. Paste its 16 characters without spaces when prompted; nothing will appear while typing:

   ```bash
   bash -c 'read -rs -p "Gmail app password: " password; echo; dotnet user-secrets set "Gmail:AppPassword" "$password" --project backend/src/Tomatime.Api >/dev/null'
   ```

4. Restart the API so it reads the new secrets. Request a reset from the login screen for an existing account and check its inbox and spam folder. In development the link defaults to `http://localhost:4200/reset-password`; open it on the same computer that runs the frontend.

For production, configure `Gmail__Address` and `Gmail__AppPassword` as environment variables or hosting secrets, and set `Frontend__ResetPasswordUrl` to the public HTTPS address ending in `/reset-password`, for example `https://your-app.example.com/reset-password`. Use the actual frontend URL, not localhost. A missing or invalid production URL causes every reset request to fail; missing Gmail credentials or SMTP failures are logged without revealing whether a requested account exists. SMTP access to `smtp.gmail.com:587` must be allowed by the host. Gmail imposes sending limits and can restrict app passwords; it is best suited to development or small deployments.

Never commit the app password or share it in chat. User secrets are local development storage, not a production secret manager.

The calendar shows sessions by local start date. Planned sessions use their planned start; other sessions use their actual start, or the planned time when the actual start is unavailable. Select a day to see its sessions, open past session details, start planned sessions, or plan a new session with the selected date prefilled.

The menu links to the authenticated session history, showing completed and cancelled sessions grouped by local start date. Each entry opens its detail page. Cancelled durations are planned focus time, not necessarily time spent. The end time is shown when recorded; older sessions and sessions cancelled after lost activity may not have one.

The menu also links to the profile, where users can see their balance and active theme, manage notifications, and change their password by providing the current one.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

### Cloudflare Pages

The Cloudflare build targets the Azure API configured in `src/app/config/api-origin.cloudflare.ts`.
For Pages Git integration, use `frontend` as the root directory, `npm run build -- --configuration production,cloudflare` as the build command, and `dist/frontend/browser` as the output directory. Set `NODE_VERSION` to the version in `frontend/.nvmrc`. The regular production build retains relative `/api` URLs for the local Docker proxy.

Configure the API's `Cors__AllowedOrigins__0` with the exact HTTPS origin of the Pages production site, and `Frontend__ResetPasswordUrl` with its HTTPS URL ending in `/reset-password`. Keep credentials and keys in App Service settings, never in the frontend or Git. Preview deployments have different origins and are not authorized by that production CORS setting.

## Progressive Web App (PWA)

The production build includes Angular's service worker and the web app manifest:

- `public/manifest.webmanifest` defines the app name, standalone display mode, colors, and install icons.
- `ngsw-config.json` defines which app files are cached for loading the application shell.
- `src/app/app.config.ts` registers the service worker only outside development mode.

The service worker caches the application shell and static assets; it does not cache API responses, which may contain user-specific data. This provides offline access to the shell and downloaded assets, but backend-dependent features still require a network connection.

To try installation, build the production version and serve its output from HTTPS (or `localhost`). The service worker is intentionally disabled by `ng serve` in development.

### Push reminders

The backend sends a Web Push reminder per registered device about five minutes before a planned session. Sessions planned closer to their start trigger an immediate dispatch; a background worker also checks every 15 seconds. The API must be running, and delivery depends on the push provider, browser and operating system. Reminders are not sent for sessions already started or past their start time; messages expire at the planned start time. If delivery fails, the worker retries until then. A crash after provider acceptance but before the delivery is recorded can result in a duplicate.

Set `WebPush:PublicKey`, `WebPush:PrivateKey` and `WebPush:Subject` (a `mailto:` contact or HTTPS URL) on the API. Generate one persistent VAPID key pair, for example with `npx web-push generate-vapid-keys`; keep the private key in user secrets locally or a secret manager in production, never in the repository. For local configuration use `dotnet user-secrets set "WebPush:PublicKey" "<public-key>" --project backend/src/Tomatime.Api`, and similarly for the private key and subject. Alternatively use environment variables `WebPush__PublicKey`, `WebPush__PrivateKey`, and `WebPush__Subject`. Changing the keys requires users to re-register their devices. Without these values the application remains usable, but enabling push returns an error.

Use the production build on HTTPS or localhost, with `/api` proxied to the backend, open **My profile**, enable notifications and register each device. Grant permission in the browser and operating system. On iOS/iPadOS Web Push requires a supported OS and installing the PWA to the Home Screen; desktop browsers also support notifications, subject to browser and OS settings. The PWA does not need to be open, but the platform may suspend delivery. Logging out removes the current device subscription; the device must be activated again after logging back in. Disabling the account preference suppresses reminders on all registered devices. A notification already accepted by a push provider cannot be recalled, but expires at the scheduled start. Supported push endpoints are limited to Google, Mozilla and Apple services. A subscription belongs to one account at a time in a browser profile.

When a reminder is clicked, the service worker focuses the most recently focused Tomatime window if one is open, without navigating away from an active timer. If no window is open, it opens the calendar; because authentication is stored per tab, the user will need to sign in again. With multiple open windows, the most recently focused window may not be the one where the user signed in. Already delivered notifications retain their original click behavior.

## Localization

The app uses ngx-translate for runtime language switching. English (`en`) is the fallback language; the initial language follows the browser preference unless the user has already selected a language. The selection is saved in browser storage.

Translation files are stored in `public/i18n/`. Add matching keys to both `en.json` and `es.json`, then use the key with the `translate` pipe in templates. The service worker pre-caches these files so the language selection remains available offline after the first successful load.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
