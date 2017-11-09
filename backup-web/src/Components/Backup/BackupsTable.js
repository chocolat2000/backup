import React, { Component } from 'react';

const shortString = function(val) {
  if (val.length < 40) return val;
  return `${val.substr(0, 37)}...`;
};

class BackupsTable extends Component {
  constructor(props) {
    super(props);
    this.state = {
      expandedlogs: {}
    };
  }

  toggleLog = logId => () => {
    this.setState(({ expandedlogs }) => {
      expandedlogs[logId] = !expandedlogs[logId];
      return { expandedlogs };
    });
  };

  render() {
    const { backups, cancel } = this.props;
    const { expandedlogs } = this.state;

    return backups && backups.length > 0 ? (
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
                  {log && log.length > 0 && shortString(log[log.length - 1])}
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
    );
  }
}

export default BackupsTable;
