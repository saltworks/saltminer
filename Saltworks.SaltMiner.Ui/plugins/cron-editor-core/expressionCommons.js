
/**
 * @typedef {"basic" | "quartz"} CronSyntax
 */

/**
 * @typedef {Object} SecondsTabState
 * @property {string} type
 * @property {number} secondInterval
 */

/**
 * @typedef {Object} MinutesTabState
 * @property {string} type
 * @property {number} minuteInterval
 */

/**
 * @typedef {Object} HourlyTabState
 * @property {string} type
 * @property {number} minutes
 * @property {number} hourInterval
 */

/**
 * @typedef {Object} DailyTabState
 * @property {string} type
 * @property {number} minutes
 * @property {number} hours
 * @property {number} dayInterval
 */

/**
 * @typedef {Object} WeeklyTabState
 * @property {string} type
 * @property {number} minutes
 * @property {number} hours
 * @property {string[]} days
 */

/**
 * @typedef {Object} MonthlyTabState
 * @property {string} type
 * @property {number} minutes
 * @property {number} hours
 * @property {number} day
 * @property {number} monthInterval
 */

/**
 * @typedef {Object} YearlyTabState
 * @property {string} type
 * @property {number} minutes
 * @property {number} hours
 * @property {number} day
 * @property {number} monthInterval
 * @property {number} yearly
 */

/**
 * @typedef {Object} AdvancedTabState
 * @property {string} type
 * @property {string} cronExpression
 */

/**
 * @typedef {(SecondsTabState | MinutesTabState | HourlyTabState | DailyTabState | WeeklyTabState | MonthlyTabState | AdvancedTabState)} UiState
 */
