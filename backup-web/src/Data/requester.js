import 'whatwg-fetch';

let AuthorizationHeader = null;

const setAuthorizationHeader = function (value) {
  AuthorizationHeader = value;
};

const GET = function (url) {
  const options = { method: 'GET' };
  if (AuthorizationHeader !== null) {
    options.headers = {
      Authorization: AuthorizationHeader
    };
  }

  return fetch(url, options).then(response => {
    if (response.ok) {
      if (response.status === 204)
        return;
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.indexOf('application/json') !== -1) {
        return response.json();
      }
      return Promise.reject('response not readable');
    }
    return Promise.reject(response.statusText);
  });
};

const POST = function (url, data) {
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

  return fetch(url, options).then(response => {
    if (response.ok) {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.indexOf('application/json') !== -1) {
        return response.json();
      }
      return Promise.reject('response not readable');
    }
    return Promise.reject(response.statusText);
  });
};

const PUT = function (url, data) {
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

  return fetch(url, options).then(response => {
    if (response.ok) {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.indexOf('application/json') !== -1) {
        return response.json();
      }
      return Promise.reject('response not readable');
    }
    return Promise.reject(response.statusText);
  });
};


const DELETE = function (url) {
  const options = { method: 'DELETE' };
  if (AuthorizationHeader !== null) {
    options.headers = {
      Authorization: AuthorizationHeader
    };
  }

  return fetch(url, options).then(response => {
    if (response.ok) {
      if (response.status === 204)
        return;
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.indexOf('application/json') !== -1) {
        return response.json();
      }
      return Promise.reject('response not readable');
    }
    return Promise.reject(response.statusText);
  });
};

export { setAuthorizationHeader, GET, POST, PUT, DELETE };
