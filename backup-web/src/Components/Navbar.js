import React from 'react';
import { Link, matchPath } from 'react-router-dom';

import { connect } from 'react-redux';
import { logout } from '../Data/actions/auth';

const getNavClassName = (pathname, props) => {
  return matchPath(pathname, props) !== null ? 'is-active' : null;
};

const Navbar = ({ location: { pathname }, logout }) => (
  <div className="tabs is-boxed">
    <div className="container">
      <ul>
        <li className={getNavClassName(pathname, { path: '/backups' })}>
          <Link to="/backups">Backups</Link>
        </li>
        <li className={getNavClassName(pathname, { path: '/servers' })}>
          <Link to="/servers">Servers</Link>
        </li>
        <li className={getNavClassName(pathname, { path: '/calendar' })}>
          <Link to="/calendar">Calendar</Link>
        </li>
        <li>
          <a onClick={logout}>Logout</a>
        </li>
      </ul>
    </div>
  </div>
);

const mapStateToProps = () => {
  return {};
};

const mapDispatchToProps = dispatch => {
  return {
    logout: () => {
      dispatch(logout());
    }
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(Navbar);
