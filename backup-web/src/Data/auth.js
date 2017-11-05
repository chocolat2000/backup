import { setAuthorizationHeader, GET, POST } from './requester';
import uuid from 'uuid/v1';

let registredAuthListeners = {};
let refreshTimeout = -1;

const isAuthenticated = function() {
  const token = sessionStorage.getItem('jwtData');
  const expires = new Date(sessionStorage.getItem('jwtExpires'));

  return token !== null && expires > Date.now();
};

const refreshAuth = function() {
  GET('/api/auth/refresh')
    .then(handleAuthResponse)
    .catch(function() {
      const expires = new Date(sessionStorage.getItem('jwtExpires'));
      const now = Date.now();
      if (now < expires) {
        var nextRefresh = (expires - now) / 2;
        refreshTimeout = setTimeout(refreshAuth, nextRefresh);
      }
    });
};

if (isAuthenticated()) {
  const token = sessionStorage.getItem('jwtData');
  setAuthorizationHeader(`Bearer ${token}`);

  const expires = new Date(sessionStorage.getItem('jwtExpires'));
  var nextRefresh = (expires - Date.now()) / 2;
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
  sessionStorage.setItem('jwtExpires', expires);
  var nextRefresh = (new Date(expires) - Date.now()) / 2;
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
