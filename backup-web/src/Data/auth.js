import { setAuthorizationHeader, POST } from './requester';

const isAuthenticated = function() {
  return (
    sessionStorage.getItem('jwtData') !== null &&
    new Date(sessionStorage.getItem('jwtExpires')) > new Date()
  );
};

if (isAuthenticated()) {
  setAuthorizationHeader(`Bearer ${sessionStorage.getItem('jwtData')}`);
}

const login = function(username, password) {
  return POST('/api/auth/login', {
    login: username,
    password: password
  }).then(({ token, expires }) => {
    setAuthorizationHeader(`Bearer ${token}`);
    sessionStorage.setItem('jwtData', token);
    sessionStorage.setItem('jwtExpires', new Date(expires));
  });
};

const logout = function() {
  sessionStorage.removeItem('jwtData');
  sessionStorage.removeItem('jwtExpires');
};

export { login, logout, isAuthenticated };
