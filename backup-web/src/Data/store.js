// @flow
import { createStore, applyMiddleware } from 'redux';
import thunk from 'redux-thunk';
import reducers from './reducers';
export type Backup = {
    +id: string,
    +startdate: Date,
    +status: string,
    +log: Array<string>
  };
  
export default createStore(reducers, applyMiddleware(thunk));
