import React, { Component } from 'react';
import uuid from 'uuid/v1';

const registredDatasource = {};

const addListener = function({ dataKey, setLoading, setData, setError }) {
  const listeners = registredDatasource[dataKey] || {};
  const listenerId = uuid();
  listeners[listenerId] = { setLoading, setData, setError };
  registredDatasource[dataKey] = listeners;
  if (listeners.data) {
    const { setData } = listeners[listenerId];
    setData && setData(listeners.data);
  }
  return function removeListener() {
    delete registredDatasource[dataKey][listenerId];
  };
};

const loadData = (dataKey, p) => {
  const listeners = registredDatasource[dataKey] || {};
  Object.keys(listeners).forEach(listenerId => {
    const { setLoading } = listeners[listenerId];
    setLoading && setLoading(true);
  });

  return {
    dataKey,
    data: Promise.resolve(p)
      .then(data => {
        const listeners = registredDatasource[dataKey] || {};
        listeners.data = data;
        Object.keys(listeners).forEach(listenerId => {
          const { setLoading, setData } = listeners[listenerId];
          setLoading && setLoading(false);
          setData && setData(data);
        });
        return data;
      })
      .catch(error => {
        const listeners = registredDatasource[dataKey] || {};
        Object.keys(listeners).forEach(listenerId => {
          const { setLoading, setError } = listeners[listenerId];
          setLoading && setLoading(false);
          setError && setError(error);
        });
      })
  };
};

const withData = function(ReactComponent, selectData) {
  return class withData extends Component {
    constructor(props) {
      super(props);
      this.state = {
        error: null,
        isLoading: false
      };
      this.mounted = false;
    }

    componentDidMount() {
      this.mounted = true;
      const dataElements = selectData(this.props);

      Object.keys(dataElements).forEach(dataElement => {
        const { dataKey } = dataElements[dataElement];
        this.removeListener = addListener({
          dataKey,
          setLoading: isLoading => {
            if (this.mounted) this.setState({ isLoading });
          },
          setData: data => {
            if (this.mounted) this.setState({ [dataElement]: data });
          },
          setError: error => {
            if (this.mounted) this.setState({ error });
          }
        });
      });
    }

    componentWillUnmount() {
      this.mounted = false;
      this.removeListener();
    }

    render() {
      return <ReactComponent data={this.state} {...this.props} />;
    }
  };
};

export { withData, loadData };
