// @flow

import type { ActionType, Action } from '../actions/backups';

import type { Backup } from '../store';

type State = {
  +isFetching: boolean,
  +list: Array<Backup>
};

const backups = (
  state: State = { isFetching: false, list: [] },
  action: Action
) => {
  switch ((action.type: ActionType)) {
    case 'REQUEST_BACKUPS': {
      return { isFetching: true, list: [{}] };
    }
    case 'RECEIVE_BACKUPS': {
      return { isFetching: false, list: action.list };
    }
    default:
      return state;
  }
};

export default backups;
