import { combineReducers } from 'redux';
import servers from './servers';
import backups from './backups';
import auth from './auth';
import errors from './errors';

export default combineReducers({
  servers,
  backups,
  auth,
  errors
});
