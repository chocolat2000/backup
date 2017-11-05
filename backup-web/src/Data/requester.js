let AuthorizationHeader = null;

const setAuthorizationHeader = function(value) {
  AuthorizationHeader = value;
};

const handleResponse = function(response) {
  const contentType = response.headers.get('content-type');
  if (response.ok) {
    if (response.status === 204) return;
    if (contentType && contentType.indexOf('application/json') !== -1) {
      return response.json();
    }
    return Promise.reject('response not readable');
  }
  if (contentType && contentType.indexOf('application/json') !== -1) {
    return new Promise((_, reject) => {
      response.json().then(({ message }) => {
        reject(message);
      });
    });
  }
  return Promise.reject(response.statusText);
};

const GET = function(url) {
  const options = { method: 'GET' };
  if (AuthorizationHeader !== null) {
    options.headers = {
      Authorization: AuthorizationHeader
    };
  }

  return fetch(url, options).then(handleResponse);
};

const POST = function(url, data) {
  const options = {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  };
  if (AuthorizationHeader !== null) {
    options.headers.Authorization = AuthorizationHeader;
  }

  return fetch(url, options).then(handleResponse);
};

const PUT = function(url, data) {
  const options = {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  };
  if (AuthorizationHeader !== null) {
    options.headers.Authorization = AuthorizationHeader;
  }

  return fetch(url, options).then(handleResponse);
};

const DELETE = function(url) {
  const options = { method: 'DELETE' };
  if (AuthorizationHeader !== null) {
    options.headers = {
      Authorization: AuthorizationHeader
    };
  }

  return fetch(url, options).then(handleResponse);
};

export { setAuthorizationHeader, GET, POST, PUT, DELETE };
