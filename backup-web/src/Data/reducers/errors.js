import { RECEIVE_ERROR, CLEAR_ERROR } from '../actions/errors';

const errors = (state = { list: [] }, action) => {
  switch (action.type) {
    case RECEIVE_ERROR: {
      return Object.assign({}, state, {
        list: state.list.concat([{ id: action.id, message: action.message }])
      });
    }
    case CLEAR_ERROR: {
      return Object.assign({}, state, {
        list: state.list.filter(error => error.id !== action.id)
      });
    }
    default:
      return state;
  }
};

export default errors;
