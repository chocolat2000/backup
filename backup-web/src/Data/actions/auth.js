// @flow

import { setAuthorizationHeader, GET, POST, PUT, DELETE } from '../requester';
import type { Dispatch } from 'redux';

import store from '../store';

export type LogguedStatus = 'loggued' | 'unlogged';

export type ActionType =
  | 'BEGIN_LOGIN'
  | 'SUCCESS_LOGIN'
  | 'ERROR_LOGIN'
  | 'END_LOGIN';

export type BEGIN_LOGIN_Action = { type: 'BEGIN_LOGIN' };
export type SUCCESS_LOGIN_Action = {
  type: 'SUCCESS_LOGIN',
  token: string,
  expires: Date
};
export type ERROR_LOGIN_Action = { type: 'ERROR_LOGIN', error: string };
export type END_LOGIN_Action = { type: 'END_LOGIN' };

export type Action =
  | BEGIN_LOGIN_Action
  | SUCCESS_LOGIN_Action
  | ERROR_LOGIN_Action
  | END_LOGIN_Action;

let refreshTimeout: number = -1;

(() => {
  const token: ?string = sessionStorage.getItem('jwtData');
  const date: ?string = sessionStorage.getItem('jwtExpires');
  if (!(token && date)) return;

  const expires: Date = new Date(date);
  if (expires > Date.now()) {
    setAuthorizationHeader(`Bearer ${token}`);

    store.dispatch({
      type: 'SUCCESS_LOGIN',
      token,
      expires
    });

    const nextRefresh: number = (expires - Date.now()) / 2;
    refreshTimeout = setTimeout(refreshAuth, nextRefresh);
  }
})();

export const login = (username: string, password: string) => (
  dispatch: Dispatch<BEGIN_LOGIN_Action>
): Promise<void> => {
  dispatch({ type: 'BEGIN_LOGIN' });
  return POST('/api/auth/login', {
    login: username,
    password: password
  }).then(handleAuthResponse, handleErrorLogin);
};

export const logout = () => (dispatch: Dispatch<END_LOGIN_Action>): void => {
  setAuthorizationHeader(null);
  sessionStorage.removeItem('jwtData');
  sessionStorage.removeItem('jwtExpires');
  if (refreshTimeout > -1) clearTimeout(refreshTimeout);
  dispatch({ type: 'END_LOGIN' });
};

const refreshAuth = (): void => {
  GET('/api/auth/refresh').then(handleAuthResponse, handleErrorLogin);
};

const handleAuthResponse = ({
  token,
  expires
}: {
  token: string,
  expires: string
}): void => {
  setAuthorizationHeader(`Bearer ${token}`);
  sessionStorage.setItem('jwtData', token);
  sessionStorage.setItem('jwtExpires', expires);

  store.dispatch({
    type: 'SUCCESS_LOGIN',
    token,
    expires: new Date(expires)
  });

  const nextRefresh: number = (new Date(expires) - Date.now()) / 2;
  refreshTimeout = setTimeout(refreshAuth, nextRefresh);
};

const handleErrorLogin = (error: string): void => {
  store.dispatch({
    type: 'ERROR_LOGIN',
    error
  });
};
