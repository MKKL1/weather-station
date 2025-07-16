## OAuth for developement
In bruno:
- On collection Auth > OAuth2.0
- Grant type: "Password Credentials"
- Fill form:
  - Access token url: http://10.147.17.33:5780/realms/weather/protocol/openid-connect/token
  - Username: horsen
  - Password: horsen
  - Client-id: weather-app
  - Scope: openid
- Then get click "Get Access Token"