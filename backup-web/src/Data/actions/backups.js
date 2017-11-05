import { GET, POST, PUT, DELETE } from '../requester';

import { addError } from './errors';

export const REQUEST_BACKUPS = 'REQUEST_BACKUPS';
export const RECEIVE_BACKUPS = 'RECEIVE_BACKUPS';

export const getBackups = serverId => dispatch => {
  dispatch({ type: REQUEST_BACKUPS, serverId });
  const uri = serverId ? `/api/backups/byserver/${serverId}` : '/api/backups';
  return GET(uri).then(
    backups => {
      dispatch({ type: RECEIVE_BACKUPS, serverId, list: backups });
    },
    message => {
      dispatch(addError(message));
    }
  );
};

export const cancel = backupId => dispatch => {
  return DELETE(`/api/backups/${backupId}`).then(
    () => {},
    message => {
      dispatch(addError(message));
    }
  );
};
