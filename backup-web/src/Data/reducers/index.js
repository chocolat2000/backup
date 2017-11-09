import { combineReducers } from 'redux';
import servers from './servers';
import backups from './backups';
import calendar from './calendar';
import auth from './auth';
import errors from './errors';

export default combineReducers({
  servers,
  backups,
  calendar,
  auth,
  errors
});
