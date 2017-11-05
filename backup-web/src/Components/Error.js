import React from 'react';
import { connect } from 'react-redux';
import { NotificationStack } from 'react-notification';

import { clearError } from '../Data/actions/errors';

const Error = ({ list, clearError }) => (
  <NotificationStack
    dismissAfter={5000}
    onDismiss={() => {}}
    notifications={list.map(({ id: key, message }) => ({
      key,
      message,
      action: 'Dismiss',
      onClick: (_, deactivate) => {
        deactivate();
        clearError(key);
      }
    }))}
  />
);

const mapStateToProps = ({ errors: { list } }) => {
  return {
    list
  };
};

const mapDispatchToProps = dispatch => {
  return {
    clearError: id => {
      dispatch(clearError(id));
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Error);
