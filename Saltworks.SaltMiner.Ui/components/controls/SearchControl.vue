<template>
  <div
    v-click-outside="dropdownClickOutside"
    :class="`searchControl ${isSearching ? 'isSearching' : ''}`"
  >
    <div class="searchContainer">
      <div v-if="Array.isArray(facetList) && facetList.length" :class="dropdownClass">
        <div class="selectedFacet" role="button" @click="dropdownToggle">
          <span>{{ currentFacet }}</span>
          <IconChevronUp />
        </div>
        <ul class="facetList">
          <li
            v-for="(facet, fidx) in facetList"
            :key="`facet-${facet.field}-${fidx}`"
            :class="getFacetClass(facet.field)"
            @click="facetClick(facet)"
          >
            {{ facet.display }}
          </li>
        </ul>
      </div>
      <div :class="Array.isArray(facetList) && facetList.length ? `searchInput` : `onlySearchInput`">
        <input
          :id="reff === '' ? labelId : reff"
          ref="inputEl"
          :type="currentInputType"
          class="form-control"
          :value="searchQuery"
          :placeholder="placeholder"
          @keyup="handleKeyUp($event)"
          @input="handleInput($event)"
        />
        <div class="searchButton" @click="handleSearch">
          <IconSearch />
        </div>
      </div>
    </div>

    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import uniqueId from 'lodash/uniqueId'
import FieldErrors from '../FieldErrors'
import IconSearch from '../../assets/svg/fi_search.svg?inline'
import IconChevronUp from '../../assets/svg/fi_chevron-up.svg?inline'

export default {
  components: {
    FieldErrors,
    IconSearch,
    IconChevronUp,
  },
  props: {
    placeholder: {
      type: String,
      default: 'Search Text....',
    },
    facets: {
      type: Array,
      default: () => [],
    },
    query: {
      type: String,
      default: '',
    },
    isSearching: {
      type: Boolean,
      default: false,
    },
    reff: {
      type: String,
      default: null,
    },
    errors: {
      type: Array,
      required: false,
      default: () => [],
    },
  },

  data() {
    return {
      toggle: false,
      searchQuery: '',
      selectedFacet: {
        field: 'all',
        display: 'All Fields',
      },
    }
  },

  computed: {
    facetList() {
      return [...this.facets]
    },
    dropdownClass() {
      return `searchFacets ${this.toggle ? 'dropdown--active' : ''}`
    },
    labelId() {
      if (this.inputId) {
        return this.inputId
      }
      return uniqueId('text-input-')
    },

    currentInputType() {
      if (this.inputtype === 'password') {
        if (this.revealPassword) {
          return 'text'
        }
      }

      return this.inputtype
    },

    currentFacet() {
      return this.facetList.length > 0
        ? this.facetList.find(
            (facet) => facet.field === this.selectedFacet.field
          )?.display || this.facetList[0].display
        : this.selectedFacet.display
    },
  },

  mounted() {
    this.searchQuery = this.query
  },

  methods: {
    handleSearch() {
      const data = {
        query: this.$refs.inputEl.value,
        facet: this.selectedFacet,
      }

      this.$emit('search', data)
    },
    handleInput(event) {
      this.searchQuery = event.target.value
      this.$emit('input', event.target.value)
    },
    handleKeyUp(event) {
      if (event.key === 'Enter') {
        this.handleSearch()
      } else {
        this.$emit('keyup', event)
      }
    },
    dropdownClickOutside() {
      this.toggle = false
    },
    dropdownToggle() {
      this.toggle = !this.toggle
    },
    facetClick(facet) {
      this.selectedFacet = facet
      this.toggle = false
      this.$emit('facet-click', facet)
    },
    getFacetClass(val) {
      return `facet ${this.selectedFacet.field === val ? 'facet--active' : ''}`
    },
  },
}
</script>

<style lang="scss" scoped>
.searchControl {
  display: flex;
  flex-direction: column;
  position: relative;
  gap: 8px;
  transition: filter 0.25s 0.5s ease-in-out;
  &.isSearching {
    filter: brightness(0.75) grayscale(2);
    transition: filter 0s ease-in-out;
  }
}

.form-control {
  display: block;
  border-radius: 8px;
  font-family: $font-opensans;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  padding: 16px;
  width: 100%;
  border: none;
}

.searchContainer {
  position: relative;
  display: flex;
}

.searchInput {
  position: relative;
  border-radius: 0px 40px 40px 0px;
  border: none;
  background-color: $brand-color-scale-1;
  min-width: 210px;

  .form-control {
    border-radius: 0px 40px 40px 0px;
    padding-right: 44px;
    background-color: $brand-color-scale-1;
    color: $brand-color-scale-6;
  }

  .searchButton {
    position: absolute;
    right: 16px;
    top: 50%;
    transform: translateY(-50%);
    cursor: pointer;
    svg {
      width: 24px;
      height: 24px;
    }
  }
}

.onlySearchInput {
  position: relative;
  border-radius: 40px 40px 40px 40px;
  border: none;
  background-color: $brand-color-scale-1;
  min-width: 210px;

  .form-control {
    border-radius: 40px 40px 40px 40px;
    padding-right: 44px;
    background-color: $brand-color-scale-1;
    color: $brand-color-scale-6;
  }

  .searchButton {
    position: absolute;
    right: 16px;
    top: 50%;
    transform: translateY(-50%);
    cursor: pointer;
    svg {
      width: 24px;
      height: 24px;
    }
  }
}

.facetList {
  z-index: 100;
  position: absolute;
  top: 100%;
  left: 0;
  overflow-x: hidden;
  overflow-y: scroll;
  max-height: 256px;

  width: 240px;
  padding: 16px;

  border: 1px solid $brand-color-scale-2;
  background: $brand-white;
  box-shadow: 0px 0px 48px rgba(0, 0, 0, 0.05);
  border-radius: 8px;
  list-style: none;
  margin: 0 0 0 0;

  display: flex;
  flex-flow: column;
  gap: 16px;

  transform-origin: top;
  opacity: 0;
  transform: scaleY(0);
  transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;

  .facet {
    width: 100%;
    font-family: $font-opensans;
    font-style: normal;
    font-weight: 700;
    font-size: 16px;
    line-height: 24px;
    color: $brand-color-scale-6;
    transform-origin: top;
    opacity: 0;
    transform: scaleY(0);
    transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;
    cursor: pointer;

    &.facet--active {
      color: $brand-primary-color;
    }
  }
}

.searchFacets {
  border-radius: 40px 0 0 40px;
  min-width: 130px;
  flex: 0 0 auto;
}
.selectedFacet {
  position: relative;
  background: $brand-white;
  padding: 16px;
  border: 1px solid $brand-color-scale-2;
  border-radius: 40px 0 0 40px;
  cursor: pointer;

  span {
    position: relative;
    padding-right: 44px;
    font-size: 14px;
    line-height: 16px;
    font-weight: 700;
    color: $brand-color-scale-6;
    font-family: $font-form-control;
    pointer-events: none;
    white-space: nowrap;
  }

  svg {
    position: absolute;
    right: 21px;
    top: 50%;
    transform: translateY(-50%) rotate(180deg);
    height: 8px;
    width: 14px;
    pointer-events: none;
  }
}

.searchFacets.dropdown--active {
  .facetList {
    opacity: 1;
    transform: scaleY(1);

    .facet {
      opacity: 1;
      transform: scaleY(1);
    }
  }
  .selectedFacet {
    svg {
      transform: translateY(-50%) rotate(0deg);
    }
  }
}
</style>
