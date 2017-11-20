// @flow
import uuid from 'uuid/v1';
import type { Dispatch } from 'redux';

export type ActionType = 'RECEIVE_ERROR' | 'CLEAR_ERROR';

export type Action = RECEIVE_ERROR_Action | CLEAR_ERROR_Action;
export type RECEIVE_ERROR_Action = {
  type: 'RECEIVE_ERROR',
  id: string,
  message: string
};
export type CLEAR_ERROR_Action = { type: 'CLEAR_ERROR', id: string };

export const addError = (message: string) => (dispatch: Dispatch<any>) => {
  const id: string = uuid();
  dispatch({ type: 'RECEIVE_ERROR', id, message });
  setTimeout(() => {
    dispatch(clearError(id));
  }, 5000);
};

export const clearError = (id: string) => (
  dispatch: Dispatch<CLEAR_ERROR_Action>
): void => {
  dispatch({ type: 'CLEAR_ERROR', id });
};
