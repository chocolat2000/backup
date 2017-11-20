// @flow
import { GET, POST, PUT, DELETE } from '../requester';
import type { Dispatch } from 'redux';
import type { Backup } from '../store';
import { addError } from './errors';

export type ActionType = 'REQUEST_BACKUPS' | 'RECEIVE_BACKUPS';

export type Action = REQUEST_BACKUPS_action | RECEIVE_BACKUPS_action;
export type REQUEST_BACKUPS_action = {
  type: 'REQUEST_BACKUPS',
  serverId: string
};
export type RECEIVE_BACKUPS_action = {
  type: 'RECEIVE_BACKUPS',
  serverId: string,
  list: Array<Backup>
};

export const getBackups = (serverId: string) => (
  dispatch: Dispatch<REQUEST_BACKUPS_action | RECEIVE_BACKUPS_action>
): Promise<void> => {
  dispatch({ type: 'REQUEST_BACKUPS', serverId });
  const uri = serverId ? `/api/backups/byserver/${serverId}` : '/api/backups';
  return GET(uri).then(
    (backups: Array<Backup>) => {
      dispatch({ type: 'RECEIVE_BACKUPS', serverId, list: backups });
    },
    (message: string) => {
      addError(message);
    }
  );
};

export const cancel = (backupId: string) => (): Promise<void> => {
  return DELETE(`/api/backups/${backupId}`).then(
    () => {},
    (message: string) => {
      addError(message);
    }
  );
};
