import { REQUEST_CALENDAR, RECEIVE_CALENDAR } from '../actions/calendar';

const calendar = (state = { isFetching: false, list: [] }, action) => {
  switch (action.type) {
    case REQUEST_CALENDAR: {
      return Object.assign({}, state, { isFetching: true, list: [] });
    }
    case RECEIVE_CALENDAR: {
      return Object.assign({}, state, { isFetching: false, list: action.list });
    }
    default:
      return state;
  }
};

export default calendar;
