import { GET, POST, PUT, DELETE } from '../requester';

import { addError } from './errors';

export const REQUEST_SERVER = 'REQUEST_SERVER';
export const RECEIVE_SERVER = 'RECEIVE_SERVER';
export const REQUEST_UPDATE_SERVER = 'REQUEST_UPDATE_SERVER';
export const RECEIVE_UPDATE_SERVER = 'RECEIVE_UPDATE_SERVER';
export const REQUEST_FOLDER = 'REQUEST_FOLDER';
export const RECEIVE_FOLDER = 'RECEIVE_FOLDER';
export const REQUEST_SERVERS = 'REQUEST_SERVERS';
export const RECEIVE_SERVERS = 'RECEIVE_SERVERS';

export const getServers = () => dispatch => {
  dispatch({ type: REQUEST_SERVERS });
  return GET('/api/servers').then(
    servers => {
      dispatch({ type: RECEIVE_SERVERS, list: servers });
    },
    message => {
      dispatch(addError(message));
    }
  );
};

export const serverDetails = (serverId, refresh) => dispatch => {
  dispatch({ type: REQUEST_SERVER, serverId });
  return GET(
    `/api/servers/${serverId}?refresh=${refresh ? 'true' : 'false'}`
  ).then(
    server => {
      dispatch({ type: RECEIVE_SERVER, serverId, server });
    },
    message => {
      dispatch({ type: RECEIVE_SERVER, serverId, server: null });
      dispatch(addError(message));
    }
  );
};

export const addServer = server => dispatch => {
  return POST(`/api/servers/${server.type}`, server).then(
    () => {
      dispatch(getServers());
    },
    message => {
      dispatch(addError(message));
    }
  );
};

export const deleteServer = serverId => dispatch => {
  return DELETE(`/api/servers/${serverId}`).then(
    () => {
      dispatch(getServers());
    },
    message => {
      dispatch(addError(message));
    }
  );
};

export const updateServer = server => dispatch => {
  const serverId = server.id;
  dispatch({ type: REQUEST_UPDATE_SERVER, serverId });
  return PUT(`/api/servers/${serverId}`, server).then(
    server => {
      dispatch({ type: RECEIVE_UPDATE_SERVER, serverId, server });
    },
    message => {
      dispatch(addError(message));
    }
  );
};

export const getContent = (serverId, folder) => dispatch => {
  dispatch({ type: REQUEST_FOLDER, serverId });
  if (typeof folder === 'string' && folder) {
    const uriPath = encodeURIComponent(folder);
    return GET(`/api/servers/${serverId}/content?folder=${uriPath}`).then(
      content => {
        dispatch({
          type: RECEIVE_FOLDER,
          folder,
          serverId,
          content
        });
      },
      message => {
        dispatch(addError(message));
      }
    );
  } else {
    return GET(`/api/servers/${serverId}/drives`).then(
      drives => {
        dispatch({
          type: RECEIVE_FOLDER,
          serverId,
          content: { folders: drives }
        });
      },
      message => {
        dispatch(addError(message));
      }
    );
  }
};
