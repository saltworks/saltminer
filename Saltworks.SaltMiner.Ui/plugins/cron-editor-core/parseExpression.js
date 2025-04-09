/**
 * parseExpression
 * Parses given expression and picks a matching tab for it.
 * It would be best if it was not dependent on cron syntax or too many configurable options.
 */

// import { UiState } from "./expressionCommons";
import { toDayAlias, isDayAlias } from "./dayAliases"

function parseSubExpr(expr) {
  const validMonths = ['JAN','FEB','MAR','APR','MAY','JUN','JUL','AUG','SEP','OCT','NOV','DEC']
  expr = expr.trim()
  let match
  if ((match = expr.match(/\*\/(\d+)/)) !== null) {
    return {
      type: "cronNumber",
      at: { type: "asterisk" },
      every: { type: "number", value: parseInt(match[1]) }
    }
  }
  if ((match = expr.match(/(\d+)\/(\d+)/)) !== null) {
    return {
      type: "cronNumber",
      at: { type: "number", value: parseInt(match[1]) },
      every: { type: "number", value: parseInt(match[2]) }
    }
  }
  if ((match = expr.match(/(\d+)/)) !== null) {
    return {
      type: "number",
      value: parseInt(match[1])
    }
  }
  if (validMonths.includes(expr)) {
    return expr
  }
  if (expr === "?") {
    return { type: "question" }
  }
  if (expr === "*") {
    return { type: "asterisk" }
  }
  throw new Error(`Unhandled subexpression: ${expr}`)
}
function parseDayOfWeek(expr) {
  expr = expr.trim()
  if (expr === "*")
    return {
      type: "asterisk"
    }
  if (expr === "?")
    return {
      type: "question"
    }

  const groups = expr.match(
    /([a-zA-Z0-9]+)(,[a-zA-Z0-9]+)?(,[a-zA-Z0-9]+)?(,[a-zA-Z0-9]+)?(,[a-zA-Z0-9]+)?(,[a-zA-Z0-9]+)?(,[a-zA-Z0-9]+)?/
  )
  if (groups === null) throw new Error(`invalid days expression: ${expr}`)
  return {
    type: "setOfDays",
    days: groups
      .slice(1)
      .map(d => d && d.replace(/,/, ""))
      .filter(d => d)
      .map(d => (!isDayAlias(d) ? toDayAlias(parseInt(d)) : d))
  }
}

const isAny = token => token.type === "question" || token.type === "asterisk"

const isAnyTime = token =>
  token.type === "asterisk" || (token.type === "number" && token.value === 0)

export const parseExpression = expression => {
  const advanced = {
    type: "advanced",
    cronExpression: expression
  }
  const groups = expression.split(" ")
  if (groups.length !== 5 && groups.length !== 6 && groups.length !== 7) {
    return advanced
  }

  // check for advanced features in the individual group (any commas)
  // to determine if advanced tab needs to be loaded. Skip index 5 which
  // is current comma separated days in the weekly tab
  let isAdvanced = false;
  groups.forEach((element, index) => {
    if (element.toString().includes(',') && index !== 5) {
      isAdvanced = true
    }
  });

  if (isAdvanced) {
    return {
      type: "advanced",
      cronExpression: expression
    }
  }
  
  const cron =
    groups.length === 7
      ? {
          seconds: parseSubExpr(groups[0]),
          minutes: parseSubExpr(groups[1]),
          hours: parseSubExpr(groups[2]),
          dayOfTheMonth: parseSubExpr(groups[3]),
          month: parseSubExpr(groups[4]),
          dayOfWeek: parseDayOfWeek(groups[5]),
          year: parseSubExpr(groups[6])
        }
      : {
          minutes: parseSubExpr(groups[0]),
          hours: parseSubExpr(groups[1]),
          dayOfTheMonth: parseSubExpr(groups[2]),
          month: parseSubExpr(groups[3]),
          dayOfWeek: parseDayOfWeek(groups[4])
        }

  if (
    cron.seconds.type === "cronNumber" &&
    isAnyTime(cron.seconds.at) &&
    cron.minutes.type === "asterisk" &&
    cron.hours.type === "asterisk" &&
    cron.dayOfTheMonth.type === "asterisk" &&
    cron.month.type === "asterisk" &&
    isAny(cron.dayOfWeek) &&
    cron.year.type === "asterisk"
  )
    return {
      type: "seconds",
      secondInterval: cron.seconds.every.value
    }
  if (
    cron.minutes.type === "cronNumber" &&
    isAnyTime(cron.minutes.at) &&
    cron.hours.type === "asterisk" &&
    cron.dayOfTheMonth.type === "asterisk" &&
    cron.month.type === "asterisk" &&
    isAny(cron.dayOfWeek) &&
    cron.year.type === "asterisk"
  )
    return {
      type: "minutes",
      seconds: cron.seconds.value,
      minuteInterval: cron.minutes.every.value
    }
  if (
    cron.seconds.type === "number" &&
    cron.minutes.type === "number" &&
    cron.hours.type === "cronNumber" &&
    isAnyTime(cron.hours.at) &&
    cron.dayOfTheMonth.type === "asterisk" &&
    cron.month.type === "asterisk" &&
    isAny(cron.dayOfWeek) &&
    cron.year.type === "asterisk"
  )
    return {
      type: "hourly",
      seconds: cron.seconds.value,
      minutes: cron.minutes.value,
      hourInterval: cron.hours.every.value
    }

  if (
    cron.seconds.type === "number" &&
    cron.minutes.type === "number" &&
    cron.hours.type === "number" &&
    cron.dayOfTheMonth.type === "cronNumber" &&
    cron.dayOfTheMonth.at.type === "asterisk" &&
    cron.month.type === "asterisk" &&
    isAny(cron.dayOfWeek) &&
    cron.year.type === "asterisk"
  )
    return {
      type: "daily",
      seconds: cron.seconds.value,
      minutes: cron.minutes.value,
      hours: cron.hours.value,
      dayInterval: cron.dayOfTheMonth.every.value
    }
  if (
    cron.seconds.type === "number" &&
    cron.minutes.type === "number" &&
    cron.hours.type === "number" &&
    isAny(cron.dayOfTheMonth) &&
    cron.month.type === "asterisk" &&
    cron.dayOfWeek.type === "setOfDays" &&
    cron.year.type === "asterisk"
  )
    return {
      type: "weekly",
      seconds: cron.seconds.value,
      minutes: cron.minutes.value,
      hours: cron.hours.value,
      days: cron.dayOfWeek.days
    }
  if (
    cron.seconds.type === 'number' &&
    cron.minutes.type === "number" &&
    cron.hours.type === "number" &&
    cron.dayOfTheMonth.type === "number" &&
    cron.month.type === "cronNumber" &&
    cron.month.at.type === "asterisk" &&
    isAny(cron.dayOfWeek) &&
    cron.year.type === "asterisk"
  )
    return {
      type: "monthly",
      seconds: cron.seconds.value,
      minutes: cron.minutes.value,
      hours: cron.hours.value,
      day: cron.dayOfTheMonth.value,
      monthInterval: cron.month.every.value
    }

  // could not find an expression pattern that matched the UI, default to 1 minute
  // return {
  //  type: "minutes",
  //  seconds: 0,
  //  minuteInterval: 1
  // }

  // return advanced with unknown expression
  return {
    type: "advanced",
    cronExpression: expression
  }
}
