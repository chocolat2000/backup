import React, { Component } from 'react';
import { withData } from '../../Data/withData';
import { serverDetails, updateServer } from '../../Data/Servers';
import { allBackups, cancel } from '../../Data/Backups';

import { formVMware, formWindows } from './Forms';

import './Details.css';

const renderForm = function(server, options) {
  switch (server.type) {
    case 'Windows':
      return formWindows(server, options);
    case 'VMware':
      return formVMware(server, options);
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
    const { data: { server } } = this.props;
    updateServer(Object.assign({}, server, form));
  };

  toggleLog = logId => () => {
    this.setState(({ expandedlogs }) => {
      expandedlogs[logId] = !expandedlogs[logId];
      return { expandedlogs };
    });
  };

  render() {
    const { data: { isLoading, server, backups } } = this.props;
    const { expandedlogs } = this.state;

    const formOptions = {
      withBackupNow: true,
      onChange: this.onChange,
      onSubmit: this.handleSubmit
    };

    if (isLoading || !server) {
      return (
        <section className="section">
          <div className="container">
            <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
          </div>
        </section>
      );
    } else {
      const { name, type } = server;
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
                {renderForm(server, formOptions)}
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
                                cancel(id);
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

export default withData(Details, ({ match }) => {
  return {
    server: serverDetails(match.params.id),
    backups: allBackups(match.params.id)
  };
});
