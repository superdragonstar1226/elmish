import * as coreJs from "core-js/shim";
import {AppRegistry} from 'react-native';
import {runnable} from './out/App';

AppRegistry.registerRunnable('AwesomeProject', runnable);