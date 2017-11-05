import React, { Component } from 'react';
import { connect } from 'react-redux';

import { serverDetails, getContent } from '../../Data/actions/severs';
import { backupNow } from '../../Data/actions/calendar';

import VMWare from './VMware';
import Windows from './Windows';

class Server extends Component {
  componentDidMount() {
    this.props.serverDetails();
  }
  render() {
    const { isFetching, type } = this.props.server;
    if (isFetching) {
      return (
        <section className="section">
          <div className="container">
            <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
          </div>
        </section>
      );
    }

    switch (type) {
      case 'VMware':
        return <VMWare {...this.props} />;
      case 'Windows':
        return <Windows {...this.props} />;
      default:
        return <div />;
    }
  }
}

const mapStateToProps = ({ servers: { list } }, { match }) => {
  const server = list[match.params.id] || {
    id: match.params.id,
    isFetching: true
  };
  return {
    server
  };
};

const mapDispatchToProps = (dispatch, { match }) => {
  return {
    serverDetails: refresh => {
      dispatch(serverDetails(match.params.id, refresh));
    },
    getContent: folder => {
      dispatch(getContent(match.params.id, folder));
    },
    backupNow: items => {
      dispatch(backupNow(match.params.id, items));
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Server);
