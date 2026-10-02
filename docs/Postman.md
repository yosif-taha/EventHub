# EventHub Postman Collection

The repository-root [EventHub.postman_collection.json](../EventHub.postman_collection.json) is an importable Postman Collection v2.1 file. Import [EventHub.postman_environment.json](../EventHub.postman_environment.json) as an environment, select it, and use it with the API profile at `https://localhost:7158` (or change `baseUrl` for another deployment).

## Before sending protected requests

1. Configure the API with its required local settings, including a JWT signing key and database connection, and start `EventHub.WebAPI` with its HTTPS profile.
2. Set `testEmail`, `testPassword`, and (when needed) `newPassword` locally in Postman. The environment intentionally contains no credentials, tokens, reset codes, Paymob HMAC, or other secrets.
3. Register an attendee if required, confirm its email through the configured email flow, then send **Authentication / Login**. Its test script saves `jwtToken`, `refreshToken`, and `userId` only to the active Postman environment.
4. For Organizer or Admin requests, log in as a user with that role. The collection deliberately uses the same `jwtToken` variable for every protected request, so switch it by logging in with the role being tested rather than keeping privileged tokens in the exported file.

The API response envelope is normally:

```json
{ "isSuccess": true, "message": "optional", "data": {} }
```

Business failures use `isSuccess: false` and include `errorCode`. Controller action results use the application's existing response convention; an HTTP `200` alone is not proof that the operation succeeded.

## Suggested workflow

1. An Admin creates a category, which saves `categoryId`.
2. An Organizer or Admin creates an event, which saves `eventId`.
3. An Attendee registers with either registration request. The successful response saves `registrationId`.
4. Use **Get My Registration / Payment Status** for the attendee-owned, backend-authoritative registration/payment state.
5. Use Admin/Organizer dashboard, event-registration, and announcement requests with the appropriate role token.

Use a disposable development database and test accounts. The collection includes destructive category/event/registration updates and deletes; Postman does not provide a transaction/rollback around a folder run.

## Payment and webhook safety

For paid events, the registration response can contain a `paymentUrl`. Opening the provider payment page or receiving its browser return does **not** prove payment success. Only Paymob's signed webhook changes the authoritative payment state. Query the attendee status endpoint after the webhook is processed.

The webhook request is intentionally populated with an invalid placeholder. It must not be used as a successful-payment simulation. To test it against an isolated Paymob transaction, replace the **entire** payload with the exact provider callback and set `paymobHmac` in your local Postman environment to the corresponding provider-supplied query HMAC. Do not export, commit, log, or share either a real callback token/signature or the configured HMAC secret. The API rejects callbacks if its HMAC secret is missing or invalid.

## Route aliases

`EventController`, `RegistrationController`, and `PaymentController` expose both REST-style and controller/action compatibility routes for a subset of their actions. The normal folders use the REST-style route where available. Every additional currently exposed conventional route is listed under **Compatibility Routes (same operations)**; they call the same handlers and are not separate features.

The collection does not include MVC routes, Swagger UI, internal hosted-service work, database operations, or Paymob provider API calls. They are not EventHub Web API controller endpoints for a Postman client to invoke.
