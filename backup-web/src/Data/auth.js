import { setAuthorizationHeader, GET, POST } from './requester';

let refreshTimeout = -1;

const isAuthenticated = function() {
  const authData = {
    token: sessionStorage.getItem('jwtData'),
    expires: sessionStorage.getItem('jwtExpires')
  };
  const isValid =
    authData.token !== null && new Date(authData.expires) > new Date();

  if (isValid) {
    handleAuthResponse(authData);
  }
  return isValid;
};

const login = function(username, password) {
  return POST('/api/auth/login', {
    login: username,
    password: password
  }).then(handleAuthResponse);
};

const logout = function() {
  sessionStorage.removeItem('jwtData');
  sessionStorage.removeItem('jwtExpires');
  if (refreshTimeout > -1) clearTimeout(refreshTimeout);
};

const handleAuthResponse = function({ token, expires }) {
  setAuthorizationHeader(`Bearer ${token}`);
  sessionStorage.setItem('jwtData', token);
  sessionStorage.setItem('jwtExpires', new Date(expires));
  var nextRefresh = (new Date(expires) - new Date()) / 2;
  refreshTimeout = setTimeout(refreshAuth, nextRefresh);
};

const refreshAuth = function() {
  GET('/api/auth/refresh').then(handleAuthResponse);
};

export { login, logout, isAuthenticated };
