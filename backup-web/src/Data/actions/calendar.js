import { GET, POST, PUT, DELETE } from '../requester';
import { addError } from './errors';

export const REQUEST_CALENDAR = 'REQUEST_CALENDAR';
export const RECEIVE_CALENDAR = 'RECEIVE_CALENDAR';

export const getCalendarEntries = server => dispatch => {
  dispatch({ type: REQUEST_CALENDAR });
  const uri = server ? `/api/calendar/${server}` : '/api/calendar';
  return GET(uri).then(
    entries => {
      dispatch({ type: RECEIVE_CALENDAR, list: entries });
    },
    message => {
      dispatch(addError(message));
    }
  );
};

export const backupNow = (server, items) => dispatch => {
  return POST('/api/calendar', {
    enabled: true,
    server,
    items,
    firstrun: new Date().toISOString(),
    periodicity: 'None'
  }).then(
    () => {
      dispatch(getCalendarEntries());
    },
    message => {
      dispatch(addError(message));
    }
  );
};
