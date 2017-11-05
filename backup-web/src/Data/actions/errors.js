import uuid from 'uuid/v1';

export const RECEIVE_ERROR = 'RECEIVE_ERROR';
export const CLEAR_ERROR = 'CLEAR_ERROR';

export const addError = message => dispatch => {
  const id = uuid();
  dispatch({ type: RECEIVE_ERROR, id, message });
  setTimeout(() => {
    clearError(id);
  }, 5000);
};

export const clearError = id => dispatch => {
  dispatch({ type: CLEAR_ERROR, id });
};
