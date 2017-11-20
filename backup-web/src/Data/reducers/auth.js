// @flow

import type { LogguedStatus, ActionType, Action } from '../actions/auth';

type State = {
  +inProgress: boolean,
  +status: LogguedStatus,
  +token?: string,
  +error?: string,
  +expires?: Date
};

const auth = (
  state: State = { inProgress: false, status: 'unlogged' },
  action: Action
): State => {
  switch ((action.type: ActionType)) {
    case 'BEGIN_LOGIN':
      return { inProgress: true, status: 'unlogged', error: '' };
    case 'SUCCESS_LOGIN': {
      const { token, expires } = action;
      return {
        inProgress: false,
        status: 'loggued',
        token,
        expires
      };
    }
    case 'ERROR_LOGIN': {
      const { error } = action;
      return { inProgress: false, status: 'unlogged', error };
    }
    case 'END_LOGIN': {
      return { inProgress: false, status: 'unlogged' };
    }
    default:
      return state;
  }
};

export default auth;
