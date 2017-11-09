import React, { Component } from 'react';
import { connect } from 'react-redux';

import { serverDetails, updateServer } from '../../Data/actions/severs';
import { getBackups, cancel } from '../../Data/actions/backups';

import { FormVMware, FormWindows } from './Forms';
import BackupsTable from '../Backup/BackupsTable';

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

class Details extends Component {
  constructor(props) {
    super(props);
    this.state = {
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

  componentDidMount() {
    this.props.serverDetails();
    this.props.getBackups();
  }

  render() {
    const { server, backups, cancel } = this.props;
    const { isFetching, name, type } = server;

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
            <BackupsTable backups={backups} cancel={cancel} />
          </div>
        </section>
      );
    }
  }
}

const mapStateToProps = (
  { servers: { list: sList }, backups: { list: bList } },
  { match }
) => {
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
    cancel: id => {
      dispatch(cancel(id));
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Details);
