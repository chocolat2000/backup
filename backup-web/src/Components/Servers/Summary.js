import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Link } from 'react-router-dom';

import { getServers, deleteServer } from '../../Data/actions/severs';

class Summary extends Component {
  componentDidMount() {
    this.props.getServers();
  }

  render() {
    const { isFetching, servers, deleteServer } = this.props;
    const content = isFetching ? (
      <div style={{ marginBottom: '1.5rem' }}>
        <i className="fa fa-spinner fa-pulse fa-3x fa-fw" />
      </div>
    ) : (
      <table className="table is-striped is-hoverable is-fullwidth">
        <thead>
          <tr>
            <th>
              <abbr title="Remove">Rem</abbr>
            </th>
            <th>Name</th>
            <th>Ip / Host</th>
            <th>Type</th>
          </tr>
        </thead>
        <tbody>
          {Object.keys(servers).map(id => (
            <tr key={id}>
              <th>
                <button className="delete" onClick={() => deleteServer(id)} />
              </th>
              <td>
                <Link to={`/servers/details/${id}`}>{servers[id].name}</Link>
              </td>
              <td>{servers[id].ip}</td>
              <td>{servers[id].type}</td>
            </tr>
          ))}
        </tbody>
      </table>
    );

    return (
      <section className="section">
        <div className="container">{content}</div>
        <div className="container">
          <div className="control">
            <Link className="button is-primary" to={'/servers/add'}>
              Add
            </Link>
          </div>
        </div>
      </section>
    );
  }
}

const mapStateToProps = ({ servers: { isFetching, list } }) => {
  return {
    isFetching,
    servers: list
  };
};

const mapDispatchToProps = dispatch => {
  return {
    getServers: () => {
      dispatch(getServers());
    },
    deleteServer: id => {
      dispatch(deleteServer(id));
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Summary);
