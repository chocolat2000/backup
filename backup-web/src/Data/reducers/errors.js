// @flow

import type { Action, ActionType } from '../actions/errors';

type ErrorMessage = {
  +id: string,
  +message: string
};

type State = {
  +list: Array<ErrorMessage>
};

const errors = (state: State = { list: [] }, action: Action) => {
  switch ((action.type: ActionType)) {
    case 'RECEIVE_ERROR': {
      return Object.assign({}, state, {
        list: state.list.concat([{ id: action.id, message: action.message }])
      });
    }
    case 'CLEAR_ERROR': {
      return Object.assign({}, state, {
        list: state.list.filter(error => error.id !== action.id)
      });
    }
    default:
      return state;
  }
};

export default errors;
