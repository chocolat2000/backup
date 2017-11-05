import 'es6-promise/auto';
import 'isomorphic-fetch';

import React from 'react';
import ReactDOM from 'react-dom';
import App from './App';
import Error from './Components/Error';
import { Provider } from 'react-redux';
import store from './Data/store';

//import registerServiceWorker from './registerServiceWorker';

ReactDOM.render(
  <Provider store={store}>
    <App />
  </Provider>,
  document.getElementById('root')
);
ReactDOM.render(
  <Provider store={store}>
    <Error />
  </Provider>,
  document.getElementById('errors')
);

//registerServiceWorker();
