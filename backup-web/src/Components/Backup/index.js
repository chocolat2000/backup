import React, { Component } from 'react';
import { connect } from 'react-redux';

import { getBackups, cancel } from '../../Data/actions/backups';

import BackupsTable from './BackupsTable';

class Backups extends Component {
  componentDidMount() {
    this.props.getBackups();
  }

  render() {
    const { isFetching, backups, cancel } = this.props;
    const content = isFetching ? (
      <div style={{ marginBottom: '1.5rem' }}>
        <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
      </div>
    ) : (
      <BackupsTable
        backups={backups}
        cancel={id => {
          this.props.cancel(id);
          this.props.getBackups();
        }}
      />
    );

    return (
      <section className="section">
        <div className="container">{content}</div>
      </section>
    );
  }
}

const mapDispatchToProps = dispatch => {
  return {
    getBackups: () => {
      dispatch(getBackups());
    },
    cancel: id => {
      dispatch(cancel(id));
    }
  };
};

const mapStateToProps = ({ backups: { isFetching, list } }) => {
  return {
    isFetching,
    backups: list
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Backups);
