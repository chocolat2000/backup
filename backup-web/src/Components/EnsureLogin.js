import React from 'react';

import { connect } from 'react-redux';
import Login from './Login';

import { login } from '../Data/actions/auth';

const EnsureLogin = ({ status, children, ...rest }) =>
  status === 'loggued' ? children : <Login {...rest} />;

const mapStateToProps = ({ auth: { status, inProgress, error } }) => {
  return {
    status,
    inProgress,
    error
  };
};

const mapDispatchToProps = dispatch => {
  return {
    login: (username, password) => {
      dispatch(login(username, password));
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(EnsureLogin);
