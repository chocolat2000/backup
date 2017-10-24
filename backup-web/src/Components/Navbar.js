import React from 'react';
import { Link } from 'react-router-dom';

const getNavClassName = ({ pathname, route }) => {
  const className = 'navbar-item is-tab';
  if (pathname.startsWith(route)) return `${className} is-active`;
  else return className;
};

const Navbar = ({ location: { pathname } }) => (
  <nav className="navbar has-shadow">
    <div className="container">
      <div className="navbar-tabs">
        <Link className={getNavClassName({ pathname, route: '/' })} to="/">
          Home
        </Link>
        <Link
          className={getNavClassName({ pathname, route: '/servers' })}
          to="/servers"
        >
          Servers
        </Link>
        <Link
          className={getNavClassName({ pathname, route: '/backups' })}
          to="/backups"
        >
          Backups
        </Link>
        <Link
          className={getNavClassName({ pathname, route: '/calendar' })}
          to="/calendar"
        >
          Calendar
        </Link>
      </div>
    </div>
  </nav>
);

export default Navbar;
