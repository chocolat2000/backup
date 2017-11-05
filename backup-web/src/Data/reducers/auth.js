import {
  BEGIN_LOGIN,
  SUCCESS_LOGIN,
  ERROR_LOGIN,
  END_LOGIN
} from '../actions/auth';

const auth = (state = { inProgress: false, status: 'unlogged' }, action) => {
  switch (action.type) {
    case BEGIN_LOGIN:
      return { inProgress: true, status: 'unlogged', error: '' };
    case SUCCESS_LOGIN: {
      const { token, expires } = action;
      return {
        inProgress: false,
        status: 'loggued',
        token,
        expires
      };
    }
    case ERROR_LOGIN: {
      const { error } = action;
      return { inProgress: false, error };
    }
    case END_LOGIN: {
      return { inProgress: false, status: 'unlogged' };
    }
    default:
      return state;
  }
};

export default auth;
