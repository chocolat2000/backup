import { setAuthorizationHeader, GET, POST, PUT, DELETE } from '../requester';

export const BEGIN_LOGIN = 'BEGIN_LOGIN';
export const SUCCESS_LOGIN = 'SUCCESS_LOGIN';
export const ERROR_LOGIN = 'ERROR_LOGIN';

export const login = (username, password) => dispatch => {
  dispatch({ type: BEGIN_LOGIN });
  return POST('/api/auth/login', {
    login: username,
    password: password
  }).then(
    ({ token, expires }) => {
      setAuthorizationHeader(`Bearer ${token}`);
      sessionStorage.setItem('jwtData', token);
      sessionStorage.setItem('jwtExpires', expires);

      return dispatch({
        type: SUCCESS_LOGIN,
        token,
        expires
      });
    },
    error =>
      dispatch({
        ERROR_LOGIN,
        error
      })
  );
};
