import React, { Component } from 'react';
import { connect } from 'react-redux';

import { serverDetails, updateServer } from '../../Data/actions/severs';
import { getBackups, cancel } from '../../Data/actions/backups';

import { FormVMware, FormWindows } from './Forms';

import './Details.css';

const renderForm = ({ server, ...rest }) => {
  switch (server.type) {
    case 'Windows':
      return <FormWindows server={server} {...rest} />;
    case 'VMware':
      return <FormVMware server={server} {...rest} />;
    default:
      return <h3>Unknown server type</h3>;
  }
};

const shortString = function(val) {
  if (val.length < 40) return val;
  return `${val.substr(0, 37)}...`;
};

class Details extends Component {
  constructor(props) {
    super(props);
    this.state = {
      expandedlogs: {},
      form: {}
    };
  }

  onChange = ({ target: { value, name } }) => {
    this.setState(({ form }) => {
      form[name] = value;
      return { form };
    });
  };

  handleSubmit = event => {
    event.preventDefault();

    const { form } = this.state;
    const { server } = this.props;
    this.props.updateServer(Object.assign({}, server, form));
  };

  toggleLog = logId => () => {
    this.setState(({ expandedlogs }) => {
      expandedlogs[logId] = !expandedlogs[logId];
      return { expandedlogs };
    });
  };

  componentDidMount() {
    this.props.serverDetails();
    this.props.getBackups();
  }

  render() {
    const { server, backups } = this.props;
    const { isFetching, name, type } = server;
    const { expandedlogs } = this.state;

    if (isFetching) {
      return (
        <section className="section">
          <div className="container">
            <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
          </div>
        </section>
      );
    } else {
      return (
        <section className="section">
          <div className="container">
            <div className="card">
              <div className="card-header">
                <div className="card-header-title">
                  {name} - {type}
                </div>
              </div>
              <div className="card-content">
                {renderForm({
                  server,
                  withBackupNow: true,
                  onChange: this.onChange,
                  onSubmit: this.handleSubmit
                })}
              </div>
            </div>
          </div>
          <div className="container" style={{ marginTop: '1.2rem' }}>
            <h3 className="title is-3">History</h3>
            {backups && backups.length > 0 ? (
              <table className="table is-hoverable is-fullwidth is-log">
                <thead>
                  <tr>
                    <th>Start Date</th>
                    <th>Status</th>
                    <th>Log</th>
                  </tr>
                </thead>
                <tbody>
                  {backups.map(({ id, startdate, status, log }) => {
                    const logExpanded = !!expandedlogs[id];
                    const oneRow = [
                      <tr key={id}>
                        <td>{startdate}</td>
                        <td>
                          {status === 'Running' ? (
                            <a
                              onClick={() => {
                                this.props.cancel();
                              }}
                            >
                              <span>{status}</span>
                              <span>
                                <i className="fa fa-stop-circle" />
                              </span>
                            </a>
                          ) : (
                            status
                          )}
                        </td>
                        <td>
                          <button
                            className="button is-white is-small"
                            onClick={this.toggleLog(id)}
                          >
                            <span className="icon is-small">
                              <i
                                className={`fa fa-caret-${logExpanded
                                  ? 'down'
                                  : 'right'}`}
                              />
                            </span>
                          </button>
                          {log &&
                            log.length > 0 &&
                            shortString(log[log.length - 1])}
                        </td>
                      </tr>
                    ];
                    if (logExpanded) {
                      oneRow.push(
                        <tr key={`${id}_log`}>
                          <td />
                          <td />
                          <td className="is-size-6">{log.join('\r\n')}</td>
                        </tr>
                      );
                    }
                    return oneRow;
                  })}
                </tbody>
              </table>
            ) : (
              <h4 className="is-size-6">Nothing ...</h4>
            )}
          </div>
        </section>
      );
    }
  }
}

const mapStateToProps = ({ servers: { list: sList }, backups: {list: bList} }, { match }) => {
  const server = sList[match.params.id] || {
    id: match.params.id,
    isFetching: true
  };
  return {
    server,
    backups: bList
  };
};

const mapDispatchToProps = (dispatch, { match }) => {
  return {
    serverDetails: () => {
      dispatch(serverDetails(match.params.id));
    },
    updateServer: server => {
      dispatch(updateServer(server));
    },
    getBackups: () => {
      dispatch(getBackups(match.params.id));
    },
    cancel: () => {
      dispatch(cancel(match.params.id));
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Details);
