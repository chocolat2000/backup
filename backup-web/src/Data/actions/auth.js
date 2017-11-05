import { setAuthorizationHeader, GET, POST, PUT, DELETE } from '../requester';
import store from '../store';

export const BEGIN_LOGIN = 'BEGIN_LOGIN';
export const SUCCESS_LOGIN = 'SUCCESS_LOGIN';
export const ERROR_LOGIN = 'ERROR_LOGIN';
export const END_LOGIN = 'END_LOGIN';

let refreshTimeout = -1;

(() => {
  const token = sessionStorage.getItem('jwtData');
  const expires = new Date(sessionStorage.getItem('jwtExpires'));
  if (token !== null && expires > Date.now()) {
    setAuthorizationHeader(`Bearer ${token}`);

    store.dispatch({
      type: SUCCESS_LOGIN,
      token,
      expires
    });  

    var nextRefresh = (expires - Date.now()) / 2;
    refreshTimeout = setTimeout(refreshAuth, nextRefresh);
  }
})();

export const login = (username, password) => dispatch => {
  dispatch({ type: BEGIN_LOGIN });
  return POST('/api/auth/login', {
    login: username,
    password: password
  }).then(handleAuthResponse, handleErrorLogin);
};

export const logout = () => dispatch => {
  setAuthorizationHeader(null);
  sessionStorage.removeItem('jwtData');
  sessionStorage.removeItem('jwtExpires');
  if (refreshTimeout > -1) clearTimeout(refreshTimeout);
  dispatch({ type: END_LOGIN });
};

const refreshAuth = () => {
  GET('/api/auth/refresh').then(
    handleAuthResponse,
    handleErrorLogin
  );
};

const handleAuthResponse = ({ token, expires }) => {
  setAuthorizationHeader(`Bearer ${token}`);
  sessionStorage.setItem('jwtData', token);
  sessionStorage.setItem('jwtExpires', expires);

  store.dispatch({
    type: SUCCESS_LOGIN,
    token,
    expires
  });

  const nextRefresh = (new Date(expires) - Date.now()) / 2;
  refreshTimeout = setTimeout(refreshAuth, nextRefresh);
};

const handleErrorLogin = error => {
  store.dispatch({
    type: ERROR_LOGIN,
    error
  });
};
