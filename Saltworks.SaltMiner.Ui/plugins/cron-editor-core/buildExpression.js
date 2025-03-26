// import { UiState, CronSyntax } from "./expressionCommons";
import { toDayNumber, toDayAlias } from "./dayAliases"

export function isStateValid(e) {
  if (e.type === "weekly" && e.days.length === 0) return false
  else return true
}

export const buildExpression = (syntax, state) => {
  if (syntax === "basic") {
    if (state.type === "minutes") {
      return `*/${state.minuteInterval} * * * *`
    }
    if (state.type === "hourly") {
      return `${state.minutes} */${state.hourInterval} * * *`
    }
    if (state.type === "daily") {
      return `${state.minutes} ${state.hours} */${state.dayInterval} * *`
    }
    if (state.type === "weekly") {
      const days = state.days
        .map(d => toDayNumber(d).toString())
        .sort()
        .join(",")
      return `${state.minutes} ${state.hours} * * ${days}`
    }
    if (state.type === "monthly") {
      return `${state.minutes} ${state.hours} ${state.day} */${state.monthInterval} *`
    }
    if (state.type === "advanced") {
      return state.cronExpression
    }
    throw `unknown event type: ${state}`
  } else if (syntax === "quartz") {
    if (state.type === "seconds") {
      if (state.secondInterval === null || state.secondInterval < 0) state.secondInterval = 0
      return `0/${state.secondInterval} * * * * ? *`
    }
    if (state.type === "minutes") {
      if (state.minuteInterval === null || state.minuteInterval < 0) state.minuteInterval = 0
      return `0 0/${state.minuteInterval} * * * ? *`
    }
    if (state.type === "hourly") {
      if (state.minutes === null || state.minutes < 0) state.minutes = 0
      if (state.hourInterval === null) state.hourInterval = 0
      return `0 ${state.minutes} 0/${state.hourInterval} * * ? *`
    }
    if (state.type === "daily") {
      if (state.minutes === null || state.minutes < 0) state.minutes = 0
      if (state.hours === null || state.hours < 0) state.hours = 0
      if (state.dayInterval === null || state.dayInterval < 0) state.dayInterval = 0
      return `0 ${state.minutes} ${state.hours} */${state.dayInterval} * ? *`
    }
    if (state.type === "weekly") {
      const days = state.days
        .map(d => toDayNumber(d))
        .sort()
        .map(d => toDayAlias(d))
        .join(",")
      if (state.minutes === null || state.minutes < 0) state.minutes = 0
      if (state.hours === null || state.hours < 0) state.hours = 0
      return `0 ${state.minutes} ${state.hours} ? * ${days} *`
    }
    if (state.type === "monthly") {
      if (state.minutes === null || state.minutes < 0) state.minutes = 0
      if (state.hours === null || state.hours < 0) state.hours = 0
      if (state.day === null || state.day < 0) state.day = 0
      if (state.monthInterval === null || state.monthInterval < 0) state.monthInterval = 0
      return `0 ${state.minutes} ${state.hours} ${state.day} */${state.monthInterval} ? *`
    }
    if (state.type === "advanced") {
      return state.cronExpression
    }
    throw `unknown event type: ${state}`
  }
  throw `unknown syntax: ${syntax}`
}
