import { GET, POST, PUT, DELETE } from '../requester';

export const REQUEST_BACKUPS = 'REQUEST_BACKUPS';
export const RECEIVE_BACKUPS = 'RECEIVE_BACKUPS';

export const getBackups = serverId => dispatch => {
  dispatch({ type: REQUEST_BACKUPS, serverId });
  const uri = serverId ? `/api/backups/byserver/${serverId}` : '/api/backups';
  GET(uri).then(backups => {
    dispatch({ type: RECEIVE_BACKUPS, serverId, list: backups });
  });
};

export const cancel = backupId => dispatch => {
  DELETE(`/api/backups/${backupId}`);
};
