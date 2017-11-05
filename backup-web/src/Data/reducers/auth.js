import { BEGIN_LOGIN, SUCCESS_LOGIN, ERROR_LOGIN } from '../actions/auth';

const servers = (state = { inProgress: false, status: 'unlogged' }, action) => {
  switch (action.type) {
    case BEGIN_LOGIN:
      return Object.assign({}, state, { inProgress: true });
    case SUCCESS_LOGIN: {
      const { token, expires } = action;
      return Object.assign({}, state, { inProgress: false, status: 'loggued', token, expires });
    }
    case ERROR_LOGIN: {
      const { error } = action;
      return Object.assign({}, state, { inProgress: false, error });
    }
    default:
      return state;
  }
};

export default servers;
