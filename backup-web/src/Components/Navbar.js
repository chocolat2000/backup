import React from 'react';
import { Link, withRouter, matchPath } from 'react-router-dom';

import { logout } from '../Data/auth';

const getNavClassName = (pathname, props) => {
  return matchPath(pathname, props) !== null ? 'is-active' : null;
};

const Navbar = ({ location: { pathname } }) => (
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

export default withRouter(Navbar);
