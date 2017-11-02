import { setAuthorizationHeader, GET, POST } from './requester';
import uuid from 'uuid/v1';

let registredAuthListeners = {};
let refreshTimeout = -1;

const isAuthenticated = function() {
  const authData = {
    token: sessionStorage.getItem('jwtData'),
    expires: sessionStorage.getItem('jwtExpires')
  };
  const isValid =
    authData.token !== null && new Date(authData.expires) > new Date();

  return isValid;
};

const refreshAuth = function() {
  GET('/api/auth/refresh').then(handleAuthResponse);
};

if (isAuthenticated()) {
  const token = sessionStorage.getItem('jwtData');
  const expires = sessionStorage.getItem('jwtExpires');
  setAuthorizationHeader(`Bearer ${token}`);
  var nextRefresh = (new Date(expires) - new Date()) / 2;
  refreshTimeout = setTimeout(refreshAuth, nextRefresh);
}

const login = function(username, password) {
  return POST('/api/auth/login', {
    login: username,
    password: password
  }).then(handleAuthResponse);
};

const logout = function() {
  setAuthorizationHeader(null);
  sessionStorage.removeItem('jwtData');
  sessionStorage.removeItem('jwtExpires');
  if (refreshTimeout > -1) clearTimeout(refreshTimeout);
  Object.keys(registredAuthListeners).forEach(listenerId => {
    const { unauthenticated } = registredAuthListeners[listenerId];
    unauthenticated && unauthenticated();
  });
};

const handleAuthResponse = function({ token, expires }) {
  setAuthorizationHeader(`Bearer ${token}`);
  sessionStorage.setItem('jwtData', token);
  sessionStorage.setItem('jwtExpires', new Date(expires));
  var nextRefresh = (new Date(expires) - new Date()) / 2;
  refreshTimeout = setTimeout(refreshAuth, nextRefresh);
  Object.keys(registredAuthListeners).forEach(listenerId => {
    const { authenticated } = registredAuthListeners[listenerId];
    authenticated && authenticated();
  });
};

const registerAuthListener = function(callbacks) {
  const listenerId = uuid();
  registredAuthListeners[listenerId] = callbacks;
  return function unregister() {
    delete registredAuthListeners[listenerId];
  };
};

export { login, logout, isAuthenticated, registerAuthListener };
