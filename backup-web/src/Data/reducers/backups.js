//import objectAssignDeep from 'object-assign-deep';
import { REQUEST_BACKUPS, RECEIVE_BACKUPS } from '../actions/backups';

const backups = (state = { isFetching: false, list: {} }, action) => {
  switch (action.type) {
    case REQUEST_BACKUPS: {
      return Object.assign({}, state, { isFetching: true, list: {} });
    }
    case RECEIVE_BACKUPS: {
      return Object.assign({}, state, { isFetching: false, list: action.list });
    }
    default:
      return state;
  }
};

export default backups;
