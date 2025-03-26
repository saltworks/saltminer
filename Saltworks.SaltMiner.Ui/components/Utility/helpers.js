import moment from 'moment';

function formatDate(
  timestamp = null,
  format = 'M/D/yyyy',
) {
  if (timestamp !== null) {
    const ts = moment.utc(timestamp).format(format);

    if (format.includes(':')) {
      return `${ts} GMT`;
    }
    
    return ts;
  }
  
  return '';
}

export default {
  formatDate,
}
